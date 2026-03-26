import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Policies } from './policies';
describe('Policies', () => {
  let c: Policies; let f: ComponentFixture<Policies>;
  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [Policies] }).compileComponents();
    f = TestBed.createComponent(Policies); c = f.componentInstance; await f.whenStable();
  });
  it('should create', () => { expect(c).toBeTruthy(); });
});
