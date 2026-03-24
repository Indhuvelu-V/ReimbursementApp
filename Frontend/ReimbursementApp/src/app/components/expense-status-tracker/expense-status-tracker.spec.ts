import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ExpenseStatusTracker } from './expense-status-tracker';

describe('ExpenseStatusTracker', () => {
  let component: ExpenseStatusTracker;
  let fixture: ComponentFixture<ExpenseStatusTracker>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ExpenseStatusTracker],
    }).compileComponents();

    fixture = TestBed.createComponent(ExpenseStatusTracker);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
