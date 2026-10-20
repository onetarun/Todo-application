// Mirrors TodoApi.Dtos.Tags. Tags are global: shared by every user.

export interface Tag {
  id: number;
  name: string;
  /** "#RRGGBB" */
  color: string;
}

export interface CreateTagRequest {
  name: string;
  color: string;
}

export interface UpdateTagRequest {
  name: string;
  color: string;
}
