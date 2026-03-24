import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FinanceDashboard } from './finance-dashboard';

describe('FinanceDashboard', () => {
  let component: FinanceDashboard;
  let fixture: ComponentFixture<FinanceDashboard>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FinanceDashboard],
    }).compileComponents();

    fixture = TestBed.createComponent(FinanceDashboard);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
