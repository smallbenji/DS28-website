using DS.DTOs;

namespace DS;

public static class ParticipantData
{
    public static ParticipantStatsDto Stats { get; } = new()
    {
        Days =
        [
            new() { Day = "LørdagStart", Label = "Lørdag (Start)", Count = 253 },
            new() { Day = "Søndag", Label = "Søndag", Count = 253 },
            new() { Day = "Mandag", Label = "Mandag", Count = 263 },
            new() { Day = "Tirsdag", Label = "Tirsdag", Count = 255 },
            new() { Day = "Onsdag", Label = "Onsdag", Count = 168 },
            new() { Day = "Torsdag", Label = "Torsdag", Count = 152 },
            new() { Day = "Fredag", Label = "Fredag", Count = 149 },
            new() { Day = "LørdagSlut", Label = "Lørdag (Slut)", Count = 140 },
        ],
        TotalUniqueParticipants = 285,
    };
}
