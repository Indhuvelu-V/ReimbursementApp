import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Policies } from './policies';

describe('Policies', () => {
  let component: Policies;
  let fixture: ComponentFixture<Policies>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Policies],
    }).compileComponents();

    fixture = TestBed.createComponent(Policies);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
