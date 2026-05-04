export interface ProjectCatalogueDto {
  id: string;
  name: string;
  description: string;
  owner: string;
  state: string;
  imageUrl?: string | null;
  financialGoal?: number | null;
  financialRaised?: number | null;
  endDate?: string | null;
}
