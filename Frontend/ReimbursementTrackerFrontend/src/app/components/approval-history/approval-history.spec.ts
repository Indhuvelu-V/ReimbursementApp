import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ApprovalHistory } from './approval-history';

describe('ApprovalHistory', () => {
  let component: ApprovalHistory;
  let fixture: ComponentFixture<ApprovalHistory>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [ApprovalHistory] }).compileComponents();
    fixture = TestBed.createComponent(ApprovalHistory);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => { expect(component).toBeTruthy(); });
});
