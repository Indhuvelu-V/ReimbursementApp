import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Logs } from './logs';
describe('Logs', () => {
  let c: Logs; let f: ComponentFixture<Logs>;
  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [Logs] }).compileComponents();
    f = TestBed.createComponent(Logs); c = f.componentInstance; await f.whenStable();
  });
  it('should create', () => { expect(c).toBeTruthy(); });
});
