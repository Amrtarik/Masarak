import { ComponentFixture, TestBed } from '@angular/core/testing';

import { FeaturesList } from './features-list';

describe('FeaturesList', () => {
  let component: FeaturesList;
  let fixture: ComponentFixture<FeaturesList>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FeaturesList],
    }).compileComponents();

    fixture = TestBed.createComponent(FeaturesList);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
