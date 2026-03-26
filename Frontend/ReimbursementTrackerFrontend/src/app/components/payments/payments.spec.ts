import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Payments } from './payments';
describe('Payments', () => {
  let c: Payments; let f: ComponentFixture<Payments>;
  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [Payments] }).compileComponents();
    f = TestBed.createComponent(Payments); c = f.componentInstance; await f.whenStable();
  });
  it('should create', () => { expect(c).toBeTruthy(); });
});
