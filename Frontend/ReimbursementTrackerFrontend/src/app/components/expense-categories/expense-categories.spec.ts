import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ExpenseCategories } from './expense-categories';

describe('ExpenseCategories', () => {
  let c: ExpenseCategories; let f: ComponentFixture<ExpenseCategories>;
  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [ExpenseCategories] }).compileComponents();
    f = TestBed.createComponent(ExpenseCategories); c = f.componentInstance; await f.whenStable();
  });
  it('should create', () => { expect(c).toBeTruthy(); });
});
