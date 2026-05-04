import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of } from 'rxjs';

import { ChatComponent } from './chat-component';
import { AuthStateService } from '../../../../auth/services/auth-state.service';
import { UserService } from '../../../../auth/services/user.service';
import { SendbirdService } from '../../../services/sendbird.service';

describe('ChatComponent', () => {
  let component: ChatComponent;
  let fixture: ComponentFixture<ChatComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ChatComponent],
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            paramMap: of(convertToParamMap({})),
            queryParamMap: of(convertToParamMap({})),
          },
        },
        {
          provide: AuthStateService,
          useValue: {
            currentUser: () => ({
              id: 'user-1',
              idP: 'public-1',
              userName: 'Casey',
              email: 'casey@example.com',
              preferredChatLanguage: 'es',
              showOriginalChatTextByDefault: false,
            }),
          },
        },
        {
          provide: UserService,
          useValue: {
            getUser: () => of({
              id: 'user-2',
              idP: 'public-2',
              userName: 'Robin',
              email: 'robin@example.com',
              preferredChatLanguage: 'en',
              showOriginalChatTextByDefault: false,
            }),
          },
        },
        {
          provide: SendbirdService,
          useValue: {
            connect: async () => undefined,
            disconnect: async () => undefined,
            getChannels: async () => [],
            createDirectChannel: async () => null,
            getChannel: async () => null,
            getMessages: async () => [],
            markAsRead: async () => undefined,
            startTyping: async () => undefined,
            endTyping: async () => undefined,
            sendMessage: async () => undefined,
            translateUserMessage: async (_channelUrl: string, message: unknown) => message,
            addGroupChannelHandler: () => undefined,
            removeGroupChannelHandler: () => undefined,
          },
        },
      ],
    })
    .compileComponents();

    fixture = TestBed.createComponent(ChatComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
