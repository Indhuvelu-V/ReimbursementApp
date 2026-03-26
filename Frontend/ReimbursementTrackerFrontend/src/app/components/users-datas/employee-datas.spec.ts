import { ComponentFixture, TestBed } from '@angular/core/testing';
import { EmployeeDatas } from './employee-datas';
describe('EmployeeDatas', () => {
  let c: EmployeeDatas; let f: ComponentFixture<EmployeeDatas>;
  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [EmployeeDatas] }).compileComponents();
    f = TestBed.createComponent(EmployeeDatas); c = f.componentInstance; await f.whenStable();
  });
  it('should create', () => { expect(c).toBeTruthy(); });
});
