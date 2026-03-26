import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Notifications } from './notifications';
describe('Notifications', () => {
  let c: Notifications; let f: ComponentFixture<Notifications>;
  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [Notifications] }).compileComponents();
    f = TestBed.createComponent(Notifications); c = f.componentInstance; await f.whenStable();
  });
  it('should create', () => { expect(c).toBeTruthy(); });
  it('should filter unread tab correctly', () => {
    c.notifications = [{ notificationId:'1', userId:'u1', message:'test', description:'', readStatus:'Unread', senderRole:'System', createdAt:'' }];
    c.setTab('unread');
    expect(c.filtered.length).toBe(1);
  });
});
