import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of } from 'rxjs';

import { AuthService } from '../../../../auth/services/auth.service';
import { AuthStateService } from '../../../../auth/services/auth-state.service';
import { EmailConfirmation } from './email-confirmation';

describe('EmailConfirmation', () => {
  let component: EmailConfirmation;
  let fixture: ComponentFixture<EmailConfirmation>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EmailConfirmation],
      providers: [
        AuthStateService,
        {
          provide: AuthService,
          useValue: {
            resendConfirmation: () => of({
              alreadyConfirmed: false,
              emailDeliveryEnabled: true,
              message: 'Check your inbox.'
            })
          }
        },
        {
          provide: Router,
          useValue: {
            navigate: () => Promise.resolve(true)
          }
        }
      ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(EmailConfirmation);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
