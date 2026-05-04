export interface CreateProjectRequest {
  headline?: string | null;
  imageUrls: string[];
  tags: string[];
  category?: string | null;
  goal: string;
  financialGoal?: number | null;
  problem?: string | null;
  description: string;
  collaboratorsIdP: string[];
  contacts: string[];
  extraInfo?: string | null;
  state: string;
  endsAt?: string | null;
  settingsId: string;
}
