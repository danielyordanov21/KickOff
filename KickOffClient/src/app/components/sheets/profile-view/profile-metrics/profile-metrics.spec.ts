import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ProfileMetrics } from './profile-metrics';

describe('ProfileMetrics', () => {
  let component: ProfileMetrics;
  let fixture: ComponentFixture<ProfileMetrics>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProfileMetrics]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ProfileMetrics);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
