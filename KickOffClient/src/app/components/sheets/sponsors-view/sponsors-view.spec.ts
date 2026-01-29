import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SponsorsView } from './sponsors-view';

describe('SponsorsView', () => {
  let component: SponsorsView;
  let fixture: ComponentFixture<SponsorsView>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SponsorsView]
    })
    .compileComponents();

    fixture = TestBed.createComponent(SponsorsView);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
