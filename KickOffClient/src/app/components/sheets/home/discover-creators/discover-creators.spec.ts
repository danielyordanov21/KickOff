import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';

import { DiscoverCreators } from './discover-creators';

describe('DiscoverCreators', () => {
  let component: DiscoverCreators;
  let fixture: ComponentFixture<DiscoverCreators>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DiscoverCreators],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(DiscoverCreators);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
