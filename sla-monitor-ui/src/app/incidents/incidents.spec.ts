import { ComponentFixture, TestBed } from '@angular/core/testing';
import { IncidentsComponent } from './incidents'; // Dosya adın incidents.ts ise bu doğrudur
import { HttpClientTestingModule } from '@angular/common/http/testing'; // API isteği için gerekli

describe('IncidentsComponent', () => {
  let component: IncidentsComponent;
  let fixture: ComponentFixture<IncidentsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      // Standalone component olduğu için imports içine ekliyoruz
      imports: [IncidentsComponent, HttpClientTestingModule], 
    }).compileComponents();

    fixture = TestBed.createComponent(IncidentsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});