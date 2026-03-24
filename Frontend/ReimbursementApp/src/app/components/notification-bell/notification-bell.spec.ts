import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NotificationBell } from './notification-bell';
describe('NotificationBell', () => {
  let c: NotificationBell; let f: ComponentFixture<NotificationBell>;
  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [NotificationBell] }).compileComponents();
    f = TestBed.createComponent(NotificationBell); c = f.componentInstance; await f.whenStable();
  });
  it('should create', () => { expect(c).toBeTruthy(); });
});
