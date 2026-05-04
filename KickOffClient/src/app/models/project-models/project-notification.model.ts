export interface ProjectNotificationRaw {
  id: string;
  projectId: string;
  projectName: string;
  projectUpdateId?: string | null;
  title: string;
  message: string;
  isRead: boolean;
  createdAt: string;
}

export interface ProjectNotification {
  id: string;
  projectId: string;
  projectName: string;
  projectUpdateId?: string;
  title: string;
  message: string;
  isRead: boolean;
  createdAt?: Date;
}

export interface ProjectNotificationFeedRaw {
  notifications?: ProjectNotificationRaw[] | null;
  unreadCount?: number | null;
}

export interface ProjectNotificationFeed {
  notifications: ProjectNotification[];
  unreadCount: number;
}
