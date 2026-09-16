import { ComponentFixture, TestBed } from '@angular/core/testing';

import { NlSearch } from './nl-search';

describe('NlSearch', () => {
  let component: NlSearch;
  let fixture: ComponentFixture<NlSearch>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [NlSearch],
    }).compileComponents();

    fixture = TestBed.createComponent(NlSearch);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
