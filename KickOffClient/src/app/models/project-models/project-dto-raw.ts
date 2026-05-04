import { ProjectUpdateRaw } from './project-update.model';
import { ProjectFollowState } from './project-follow.model';

export interface ProjectDtoRaw {
    id: string;
    name: string;
    headline?: string | null;
    goal: string;
    description: string;
    imageUrls?: string[];
    state: string | number;
    extraInfo?: string | null;

    owner: string;
    ownerId: string;
    ownerPublicId?: string | null;
    category?: string | null;
    financialGoal?: number | null;
    problem?: string | null;
    collaboratorsIdP?: string[];
    contacts?: string[];

    tags?: string[];
    imageBlobNames?: string[];
    backerIds?: string[];
    updates?: ProjectUpdateRaw[];
    follow?: ProjectFollowState | null;

    startDate?: string | null;
    endDate?: string | null;

    settingsId?: string | null;
}
