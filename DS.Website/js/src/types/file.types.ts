export interface ImageReferenceDto {
  publicId: string;
  url: string;
}

export interface StoredFileDto {
  id: number;
  publicId: string;
  originalName: string;
  contentType: string;
  status: string;
  purpose: string;
  width: number | null;
  height: number | null;
  url: string | null;
}
