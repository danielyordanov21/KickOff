import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { UserService } from '../../../../auth/services/user.service';
import { User } from '../../../../auth/user.model';

import { UserFloatingPanel } from './user-floating-panel';

describe('UserFloatingPanel', () => {
  const buildUser = (id: string): User => ({
    id,
    idP: id,
    userName: `User ${id}`,
    email: `${id}@kickoff.test`,
  });

  const createPanel = async (id: string): Promise<ComponentFixture<UserFloatingPanel>> => {
    const fixture = TestBed.createComponent(UserFloatingPanel);

    fixture.componentRef.setInput('userId', id);
    fixture.componentRef.setInput('previewUser', {
      id,
      userName: `User ${id}`,
    });
    fixture.detectChanges();
    await fixture.whenStable();

    return fixture;
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [UserFloatingPanel],
      providers: [
        provideRouter([]),
        {
          provide: UserService,
          useValue: {
            getUser: (id: string) => of(buildUser(id)),
            followUser: () => of(void 0),
            unfollowUser: () => of(void 0),
          },
        },
      ],
    })
    .compileComponents();
  });

  it('should create', async () => {
    const fixture = await createPanel('alpha');
    const component = fixture.componentInstance;

    expect(component).toBeTruthy();

    fixture.destroy();
  });

  it('closes the previous panel before opening the next one', async () => {
    const firstFixture = await createPanel('alpha');
    const secondFixture = await createPanel('beta');
    const firstComponent = firstFixture.componentInstance;
    const secondComponent = secondFixture.componentInstance;

    firstComponent.openPanel();
    secondComponent.openPanel();

    expect(firstComponent.isOpen()).toBe(false);
    expect(secondComponent.isOpen()).toBe(true);

    firstFixture.destroy();
    secondFixture.destroy();
  });

  it('clears the previous panel close timer when another panel takes over', async () => {
    const firstFixture = await createPanel('alpha');
    const secondFixture = await createPanel('beta');
    const firstComponent = firstFixture.componentInstance;
    const secondComponent = secondFixture.componentInstance;

    firstComponent.openPanel();
    firstComponent.scheduleClose();

    expect((firstComponent as unknown as { closeTimeoutId: ReturnType<typeof setTimeout> | null }).closeTimeoutId)
      .not.toBeNull();

    secondComponent.openPanel();

    expect((firstComponent as unknown as { closeTimeoutId: ReturnType<typeof setTimeout> | null }).closeTimeoutId)
      .toBeNull();
    expect(firstComponent.isOpen()).toBe(false);
    expect(secondComponent.isOpen()).toBe(true);

    firstFixture.destroy();
    secondFixture.destroy();
  });
});
