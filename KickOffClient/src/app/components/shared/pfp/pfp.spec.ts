import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Pfp } from './pfp';

describe('Pfp', () => {
  let component: Pfp;
  let fixture: ComponentFixture<Pfp>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Pfp]
    })
    .compileComponents();

    fixture = TestBed.createComponent(Pfp);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
