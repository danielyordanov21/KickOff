export interface ProjectUpdateRaw {
  id: string;
  projectId: string;
  title: string;
  content: string;
  authorUserId: string;
  authorName: string;
  createdAt?: string | null;
  updatedAt?: string | null;
  isEdited?: boolean | null;
}

export interface ProjectUpdate {
  id: string;
  projectId: string;
  title: string;
  content: string;
  authorUserId: string;
  authorName: string;
  createdAt?: Date | null;
  updatedAt?: Date | null;
  isEdited: boolean;
}
