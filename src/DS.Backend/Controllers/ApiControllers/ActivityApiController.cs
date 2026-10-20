using DS.Application.Services.Google;
using DS.Application.Services.Activities;
using DS.Domain;
using DS.Domain.DTOs;
using DS.Domain.Models;
using DS.Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DS.Backend.Controllers
{
    [Authorize(Roles = nameof(AppRoles.ActivityView))]
    [Route("api/v1/activity")]
    public class ActivityApiController(
        ActivityService activityService,
        ActivityTeamService activityTeamService,
        UserManager<User> userManager,
        IGoogleAuthService googleAuth,
        IGoogleDriveService googleDrive,
        ILogger<ActivityApiController> logger) : Controller
    {
        private bool IsActivityAdmin()
        {
            return User.IsInRole(nameof(AppRoles.ActivityAdmin));
        }

        private async Task<bool> HasAccessAsync(int teamId, bool isAdmin = false)
        {
            if (IsActivityAdmin()) return true;

            var userId = userManager.GetUserId(User);
            return await activityTeamService.HasAccessToTeam(userId, teamId, isAdmin);
        }

        [HttpGet("teams")]
        public async Task<IActionResult> GetTeams()
        {
            var teams = IsActivityAdmin()
                ? await activityTeamService.GetAllActivityTeamsAsync()
                : await activityTeamService.GetUserActivityTeamAsync(userManager.GetUserId(HttpContext.User));

            return Ok(teams.Select(t => new ActivityTeamDto(t)).ToList());
        }

        [HttpPost("team/{teamId:int}/activity/add")]
        public async Task<IActionResult> AddActivity([FromBody] ActivityDto data, int teamId)
        {
            if (!await HasAccessAsync(teamId, true)) return StatusCode(StatusCodes.Status403Forbidden);

            await activityService.AddActivity(teamId, new Activity(data));

            return Ok();
        }

        [HttpPost("teams/add")]
        public async Task<IActionResult> AddTeam([FromBody] ActivityTeamDto activityTeam)
        {
            var userId = userManager.GetUserId(HttpContext.User);
            await activityTeamService.AddTeam(activityTeam.Name, userId);

            return Ok();
        }

        [HttpGet("users/search")]
        public async Task<IActionResult> SearchUsers([FromQuery] string search, [FromQuery] int teamId)
        {
            if (string.IsNullOrWhiteSpace(search)) return Ok(new List<UserDto>());

            var users = await activityTeamService.SearchActivityUsersAsync(search.Trim(), teamId);

            return Ok(users.Select(u => new UserDto(u)).ToList());
        }

        [HttpPost("team/{teamId:int}/member/add")]
        public async Task<IActionResult> AddMember([FromBody] ActivityTeamMembershipDto data, int teamId)
        {
            if (!await HasAccessAsync(teamId, true)) return StatusCode(StatusCodes.Status403Forbidden);

            if (data == null || string.IsNullOrWhiteSpace(data.UserId)) return BadRequest("Invalid request body.");

            var success = await activityTeamService.AddMember(teamId, data.UserId, data.IsAdmin);
            if (!success) return NotFound("User not found.");

            return Ok();
        }

        [HttpPost("team/{teamId:int}/member/remove")]
        public async Task<IActionResult> RemoveMember([FromBody] ActivityTeamMembershipDto data, int teamId)
        {
            var userId = userManager.GetUserId(User);
            if (!await HasAccessAsync(teamId, true)) return StatusCode(StatusCodes.Status403Forbidden);

            if (data == null || string.IsNullOrWhiteSpace(data.UserId)) return BadRequest("Invalid request body.");

            if (data.UserId == userId) return BadRequest("You cannot remove yourself from the team.");

            var success = await activityTeamService.RemoveMember(teamId, data.UserId);
            if (!success) return NotFound("Membership not found.");

            return Ok();
        }

        [HttpGet("activity/{activityId:int}")]
        public async Task<IActionResult> GetActivity(int activityId)
        {
            var activity = await activityService.GetActivity(activityId);
            if (activity == null) return NotFound();

            if (!await HasAccessAsync(activity.ActivityTeamId, false)) return StatusCode(StatusCodes.Status403Forbidden);

            var result = new ActivityDto(activity)
            {
                CanEdit = await HasAccessAsync(activity.ActivityTeamId, true)
            };

            return Ok(result);
        }

        [HttpPut("activity/{activityId:int}")]
        public async Task<IActionResult> UpdateActivity([FromBody] ActivityUpdateDto data, int activityId)
        {
            var activity = await activityService.GetActivity(activityId);
            if (activity == null) return NotFound();

            if (!await HasAccessAsync(activity.ActivityTeamId, true)) return StatusCode(StatusCodes.Status403Forbidden);

            if (data == null || string.IsNullOrWhiteSpace(data.Name) || data.ReviewVersion == null) return BadRequest("Navn og version skal angives.");

            var userId = userManager.GetUserId(User);
            var result = await activityService.UpdateActivityAsync(activityId, data, userId);

            return ActivityChangeResponse(result);
        }

        [HttpPost("activity/{activityId:int}/submit")]
        public async Task<IActionResult> SubmitActivity([FromBody] ActivityReviewRequestDto data, int activityId)
        {
            var activity = await activityService.GetActivity(activityId);
            if (activity == null) return NotFound();

            if (!await HasAccessAsync(activity.ActivityTeamId, true)) return Forbid();

            if (data?.ReviewVersion == null) return BadRequest("Version skal angives.");

            var result = await activityService.ReviewActivity(activityId, data.ReviewVersion.Value, ActivityReviewAction.Submit, userManager.GetUserId(User));

            return ActivityChangeResponse(result);
        }

        [Authorize(Roles = nameof(AppRoles.ActivityAdmin))]
        [HttpPost("activity/{activityId:int}/approve")]
        public async Task<IActionResult> ApproveActivity([FromBody] ActivityReviewRequestDto data, int activityId)
        {
            if (data?.ReviewVersion == null) return BadRequest("Version skal angives.");

            var result = await activityService.ReviewActivity(activityId, data.ReviewVersion.Value, ActivityReviewAction.Approve, userManager.GetUserId(User));

            return ActivityChangeResponse(result);
        }

        [Authorize(Roles = nameof(AppRoles.ActivityAdmin))]
        [HttpPost("activity/{activityId:int}/request-changes")]
        public async Task<IActionResult> RequestActivityChanges([FromBody] ActivityReviewRequestDto data, int activityId)
        {
            if (data?.ReviewVersion == null) return BadRequest("Version skal angives.");

            var result = await activityService.ReviewActivity(activityId, data.ReviewVersion.Value, ActivityReviewAction.RequestChanges, userManager.GetUserId(User), data.Feedback);

            return ActivityChangeResponse(result);
        }

        private IActionResult ActivityChangeResponse(ActivityChangeResult result)
        {
            switch (result)
            {
                case ActivityChangeResult.Success:
                    return Ok();
                case ActivityChangeResult.NotFound:
                    return NotFound();
                case ActivityChangeResult.Conflict:
                    return Conflict("Aktiviteten er ændret af en anden. Genindlæs og gennemgå den nyeste version, før du fortsætter.");
                case ActivityChangeResult.InvalidState:
                    return Conflict("Handlingen er ikke mulig med aktivitetens nuværende status. Genindlæs aktiviteten.");
                case ActivityChangeResult.Incomplete:
                    return BadRequest("Udfyld navn, katalognavn, resumé og beskrivelse før indsendelse eller godkendelse.");
                case ActivityChangeResult.InvalidFeedback:
                    return BadRequest("Skriv en begrundelse på højst 4.000 tegn, når aktiviteten sendes retur.");
                default:
                    return BadRequest("Billedet findes ikke, eller du har ikke adgang til at bruge det.");
            }
        }

        [HttpPost("team/{teamId:int}/invite")]
        public async Task<IActionResult> InviteUser([FromBody] ActivityTeamInviteDto data, int teamId)
        {
            if (!await HasAccessAsync(teamId, true)) return StatusCode(StatusCodes.Status403Forbidden);

            if (data == null || string.IsNullOrWhiteSpace(data.Email)) return BadRequest("Invalid request body.");

            var invitationId = await activityTeamService.CreateInvitationAsync(
                teamId,
                data.Email.Trim(),
                data.IsAdmin
            );
            if (invitationId == null) return NotFound("Team not found.");

            var link = $"{Request.Scheme}://{Request.Host}/invitation/{invitationId}";

            return Ok(new ActivityTeamInviteLinkDto
            {
                Link = link,
                Email = data.Email.Trim()
            });
        }

        [HttpGet("activity/{activityId:int}/files")]
        public async Task<IActionResult> GetFiles(int activityId, [FromQuery] string folderId)
        {
            var activity = await activityService.GetActivity(activityId);
            if (activity == null) return NotFound();

            if (!await HasAccessAsync(activity.ActivityTeamId, false)) return StatusCode(StatusCodes.Status403Forbidden);

            if (!googleAuth.IsConfigured) return StatusCode(StatusCodes.Status503ServiceUnavailable, "Google Drive er ikke konfigureret.");
            if (!await googleAuth.IsConnectedAsync()) return StatusCode(StatusCodes.Status503ServiceUnavailable, "Google Drev er ikke forbundet.");

            List<DriveFileInfo> files;
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                files = await googleDrive.GetActivityFilesAsync(activityId, folderId, timeout.Token);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Hentning af filer fra Google Drive fejlede for aktivitet {ActivityId}.", activityId);
                return StatusCode(StatusCodes.Status502BadGateway, "Filerne kunne ikke hentes fra Google Drive.");
            }

            if (files == null) return NotFound("Mappen findes ikke.");

            var created = await activityService.GetFilesAsync(activityId);
            var result = new List<ActivityFileDto>();

            foreach (var file in files)
            {
                var record = created.FirstOrDefault(f => f.DriveFileId == file.Id);

                result.Add(new ActivityFileDto
                {
                    Id = file.Id,
                    Name = file.Name,
                    Kind = GoogleFileTypes.KindForMimeType(file.MimeType),
                    Url = GoogleFileTypes.OpenUrl(file.MimeType, file.Id),
                    ModifiedAt = file.ModifiedAt,
                    CreatedBy = record?.CreatedBy?.GetFullName(),
                    CreatedAt = record?.CreatedAt,
                });
            }

            return Ok(result);
        }

        [HttpPost("activity/{activityId:int}/file/add")]
        public async Task<IActionResult> AddFile([FromBody] ActivityFileCreateDto data, int activityId)
        {
            var activity = await activityService.GetActivity(activityId);
            if (activity == null) return NotFound();

            if (!await HasAccessAsync(activity.ActivityTeamId, true)) return StatusCode(StatusCodes.Status403Forbidden);

            if (!googleAuth.IsConfigured) return StatusCode(StatusCodes.Status503ServiceUnavailable, "Google Drive er ikke konfigureret.");
            if (!await googleAuth.IsConnectedAsync()) return StatusCode(StatusCodes.Status503ServiceUnavailable, "Google Drev er ikke forbundet.");

            var kind = string.IsNullOrWhiteSpace(data?.Kind) ? GoogleFileTypes.DocumentKind : data.Kind.Trim().ToLowerInvariant();
            if (!GoogleFileTypes.IsSupportedKind(kind)) return BadRequest("Ukendt filtype.");

            var fallbackTitle = kind == GoogleFileTypes.FolderKind ? "Ny mappe" : activity.Name;
            var title = string.IsNullOrWhiteSpace(data?.Title) ? fallbackTitle : data.Title.Trim();
            if (string.IsNullOrWhiteSpace(title)) return BadRequest("Navnet mangler.");
            if (title.Length > 200) return BadRequest("Navnet må højst være 200 tegn.");

            var userId = userManager.GetUserId(User);

            string driveFileId;
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                driveFileId = await googleDrive.CreateActivityFileAsync(activityId, activity.Name, kind, title, data?.FolderId, timeout.Token);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Oprettelse af Google-fil fejlede for aktivitet {ActivityId}.", activityId);
                return StatusCode(StatusCodes.Status502BadGateway, "Filen eller mappen kunne ikke oprettes i Google Drive.");
            }

            if (driveFileId == null) return NotFound("Mappen findes ikke.");

            var file = await activityService.AddFileAsync(activityId, driveFileId, title, userId);
            var createdBy = await userManager.FindByIdAsync(userId);
            var mimeType = GoogleFileTypes.MimeTypeForKind(kind);

            return Ok(new ActivityFileDto
            {
                Id = file.DriveFileId,
                Name = file.Title,
                Kind = kind,
                Url = GoogleFileTypes.OpenUrl(mimeType, file.DriveFileId),
                CreatedBy = createdBy?.GetFullName(),
                CreatedAt = file.CreatedAt,
            });
        }
    }
}
