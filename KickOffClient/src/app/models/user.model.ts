export interface User {
  id: string;
  idP: string;
  role: string;
  username: string;
  profilePictureUrl?: string;
  projectIds: string[];
  followerIdsP: string[];
  followingIdsP: string[];
  state: string;
}