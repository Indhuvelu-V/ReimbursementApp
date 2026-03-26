import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FileViewModal } from './file-view-modal';

describe('FileViewModal', () => {
  let component: FileViewModal;
  let fixture: ComponentFixture<FileViewModal>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [FileViewModal] }).compileComponents();
    fixture = TestBed.createComponent(FileViewModal);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => { expect(component).toBeTruthy(); });
});
