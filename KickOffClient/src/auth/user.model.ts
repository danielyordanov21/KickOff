import { ProjectCatalogueDto } from '../app/models/project-models/project-catalogue.model';

export interface ProfileConnection {
  id: string;
  idP: string;
  userName: string;
  profilePictureUrl?: string;
  state?: string;
}

export interface User {
  id: string;
  idP?: string;
  role?: string;
  roles?: string[];
  userName: string;
  email: string;
  emailConfirmed?: boolean;
  profilePictureUrl?: string;
  preferredChatLanguage?: string | null;
  showOriginalChatTextByDefault?: boolean;
  canDeleteAccount?: boolean | null;
  deleteAccountRestriction?: string | null;
  projects?: ProjectCatalogueDto[];
  backedProjects?: ProjectCatalogueDto[];
  followers?: ProfileConnection[];
  following?: ProfileConnection[];
  projectIds?: string[];
  followerIdsP?: string[];
  followingIdsP?: string[];
  state?: string;
}
