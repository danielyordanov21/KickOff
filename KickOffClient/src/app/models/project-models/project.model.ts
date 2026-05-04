import { ProjectUpdate } from './project-update.model';
import { ProjectFollowState } from './project-follow.model';

export class Project {
  id!: string;
  name!: string;
  headline?: string | null;
  goal!: string;
  description!: string;
  imageUrls?: string[];
  state!: string;
  extraInfo?: string | null;

  owner!: string;
  ownerId!: string;
  ownerPublicId?: string;
  category?: string | null;
  financialGoal?: number | null;
  problem?: string | null;
  imageBlobNames!: string[];
  collaboratorsIdP!: string[];
  contacts!: string[];

  tags!: string[];
  backerIds!: string[];
  updates!: ProjectUpdate[];
  follow!: ProjectFollowState;

  startDate?: Date | null;
  endDate?: Date | null;

  // If user = owner
  settingsId?: string;
}
