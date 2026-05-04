import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import SendbirdChat, { User, UserEventHandler } from '@sendbird/chat';
import {
  GroupChannel,
  GroupChannelHandler,
  GroupChannelListQueryParams,
  GroupChannelModule,
  SendbirdGroupChat,
} from '@sendbird/chat/groupChannel';
import { BaseMessage, FileMessage, UserMessage } from '@sendbird/chat/message';
import { firstValueFrom } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class SendbirdService {
  private readonly apiUrl = '/api/chat';
  private readonly http = inject(HttpClient);
  private readonly sb: SendbirdGroupChat = SendbirdChat.init({
    appId: 'E148F321-A339-4D9A-BD94-13E4D49EDCE5',
    modules: [new GroupChannelModule()],
  });

  readonly totalUnreadMessageCount = signal(0);

  private activeConnectionPromise: Promise<User> | null = null;
  private connectingUserId: string | null = null;
  private readonly unreadHandlerKey = 'kickoff-chat-unread';
  private hasUnreadHandler = false;

  async connect(userId: string, preferredLanguages: string[] = []): Promise<User> {
    if (this.sb.currentUser?.userId === userId) {
      this.ensureUnreadHandler();
      await this.syncPreferredLanguages(preferredLanguages);
      await this.refreshUnreadCount();
      return this.sb.currentUser;
    }

    if (this.activeConnectionPromise && this.connectingUserId === userId) {
      return this.activeConnectionPromise;
    }

    this.connectingUserId = userId;
    this.activeConnectionPromise = this.establishConnection(userId);

    try {
      const user = await this.activeConnectionPromise;
      this.ensureUnreadHandler();
      await this.syncPreferredLanguages(preferredLanguages);
      await this.refreshUnreadCount();
      return user;
    } finally {
      this.activeConnectionPromise = null;
      this.connectingUserId = null;
    }
  }

  async disconnect(): Promise<void> {
    this.activeConnectionPromise = null;
    this.connectingUserId = null;
    this.totalUnreadMessageCount.set(0);

    if (this.hasUnreadHandler) {
      this.sb.removeUserEventHandler(this.unreadHandlerKey);
      this.hasUnreadHandler = false;
    }

    if (this.sb.currentUser) {
      await this.sb.disconnect();
    }
  }

  addGroupChannelHandler(key: string, handler: GroupChannelHandler): void {
    this.sb.groupChannel.addGroupChannelHandler(key, handler);
  }

  removeGroupChannelHandler(key: string): void {
    this.sb.groupChannel.removeGroupChannelHandler(key);
  }

  async getChannels(params?: GroupChannelListQueryParams): Promise<GroupChannel[]> {
    const limit = params?.limit ?? 20;
    const query = this.sb.groupChannel.createMyGroupChannelListQuery({
      includeEmpty: true,
      limit,
      ...params,
    });

    const channels: GroupChannel[] = [];

    while (query.hasNext && channels.length < limit) {
      const nextChannels = await query.next();
      channels.push(...nextChannels);
    }

    return channels.slice(0, limit);
  }

  async createDirectChannel(currentUserId: string, otherUserId: string): Promise<GroupChannel> {
    if (currentUserId === otherUserId) {
      throw new Error('You cannot start a chat with yourself.');
    }

    const { channelUrl } = await firstValueFrom(
      this.http.post<{ channelUrl: string }>(`${this.apiUrl}/channel`, {
        user1: currentUserId,
        user2: otherUserId,
      }),
    );

    if (!channelUrl?.trim()) {
      throw new Error('The chat channel response did not include a valid channel URL.');
    }

    return this.getChannelWithRetry(channelUrl);
  }

  async getChannel(channelUrl: string): Promise<GroupChannel> {
    return this.sb.groupChannel.getChannel(channelUrl);
  }

  async getMessages(channelUrl: string, limit = 40): Promise<BaseMessage[]> {
    const channel = await this.getChannel(channelUrl);

    const messages = await channel.getMessagesByTimestamp(Date.now(), {
      prevResultSize: limit,
      nextResultSize: 0,
      isInclusive: true,
      reverse: false,
    });

    return [...messages].sort((left, right) => left.createdAt - right.createdAt);
  }

  async markAsRead(channelUrl: string): Promise<void> {
    const channel = await this.getChannel(channelUrl);
    await channel.markAsRead();
  }

  async startTyping(channelUrl: string): Promise<void> {
    const channel = await this.getChannel(channelUrl);
    await channel.startTyping();
  }

  async endTyping(channelUrl: string): Promise<void> {
    const channel = await this.getChannel(channelUrl);
    await channel.endTyping();
  }

  async sendMessage(
    channelUrl: string,
    message: string,
    translationTargetLanguages: string[] = [],
  ): Promise<BaseMessage> {
    const channel = await this.getChannel(channelUrl);

    return new Promise((resolve, reject) => {
      channel
        .sendUserMessage({
          message,
          translationTargetLanguages: this.normalizeLanguages(translationTargetLanguages),
        })
        .onSucceeded(sentMessage => resolve(sentMessage))
        .onFailed(error => reject(error));
    });
  }

  async sendImageMessage(channelUrl: string, file: File): Promise<FileMessage> {
    const channel = await this.getChannel(channelUrl);

    return new Promise((resolve, reject) => {
      channel
        .sendFileMessage({
          file,
          fileName: file.name,
          mimeType: file.type,
          thumbnailSizes: [
            { maxWidth: 480, maxHeight: 480 },
            { maxWidth: 960, maxHeight: 960 },
          ],
        })
        .onSucceeded(sentMessage => resolve(sentMessage as FileMessage))
        .onFailed(error => reject(error));
    });
  }

  async translateUserMessage(
    channelUrl: string,
    targetMessage: UserMessage,
    preferredLanguage: string,
  ): Promise<UserMessage> {
    const channel = await this.getChannel(channelUrl);
    return channel.translateUserMessage(targetMessage, [preferredLanguage]);
  }

  async syncPreferredLanguages(preferredLanguages: string[]): Promise<void> {
    if (!this.sb.currentUser) {
      return;
    }

    await this.sb.updateCurrentUserInfoWithPreferredLanguages(
      this.normalizeLanguages(preferredLanguages),
    );
  }

  private async establishConnection(userId: string): Promise<User> {
    if (this.sb.currentUser && this.sb.currentUser.userId !== userId) {
      await this.sb.disconnect();
    }

    const { token } = await firstValueFrom(this.http.get<{ token: string }>(`${this.apiUrl}/token`));
    return this.sb.connect(userId, token);
  }

  private ensureUnreadHandler(): void {
    if (this.hasUnreadHandler) {
      return;
    }

    this.sb.addUserEventHandler(this.unreadHandlerKey, new UserEventHandler({
      onTotalUnreadMessageCountChanged: unreadMessageCount => {
        this.totalUnreadMessageCount.set(unreadMessageCount.groupChannelCount ?? 0);
      },
    }));

    this.hasUnreadHandler = true;
  }

  private async refreshUnreadCount(): Promise<void> {
    try {
      const unreadCount = await this.sb.groupChannel.getTotalUnreadMessageCount();
      this.totalUnreadMessageCount.set(unreadCount);
    } catch (error) {
      console.error('Failed to refresh unread chat count.', error);
    }
  }

  private async getChannelWithRetry(channelUrl: string, attempts = 3): Promise<GroupChannel> {
    let lastError: unknown;

    for (let attempt = 1; attempt <= attempts; attempt++) {
      try {
        return await this.getChannel(channelUrl);
      } catch (error) {
        lastError = error;

        if (attempt === attempts) {
          break;
        }

        await new Promise(resolve => setTimeout(resolve, attempt * 250));
      }
    }

    throw lastError instanceof Error
      ? lastError
      : new Error(`Failed to load Sendbird channel ${channelUrl}.`);
  }

  private normalizeLanguages(preferredLanguages: string[]): string[] {
    return Array.from(new Set(
      preferredLanguages
        .map(language => language.trim().toLowerCase())
        .filter(language => language.length > 0),
    ));
  }
}
