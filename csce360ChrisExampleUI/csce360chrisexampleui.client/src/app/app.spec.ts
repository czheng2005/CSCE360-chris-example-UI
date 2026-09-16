import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { App } from './app';

describe('App', () => {
  let component: App;
  let fixture: ComponentFixture<App>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [App],
      imports: [HttpClientTestingModule]
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(App);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should create the app', () => {
    component.ngOnInit();
    httpMock.expectOne('/categories').flush([]);
    httpMock.expectOne('/companies').flush([]);
    httpMock.expectOne(req => req.url === '/products').flush([]);
    expect(component).toBeTruthy();
  });

  it('should retrieve products from the server', () => {
    const mockProducts = [
      { companyName: 'Acme Co', productName: 'Widget', price: 20, category: 'Hardware', onSale: false },
      { companyName: 'Acme Co', productName: 'Gadget', price: 25, category: 'Hardware', onSale: true }
    ];

    component.ngOnInit();

    httpMock.expectOne('/categories').flush([]);
    httpMock.expectOne('/companies').flush([]);

    const req = httpMock.expectOne(r => r.url === '/products');
    expect(req.request.method).toEqual('GET');
    req.flush(mockProducts);

    expect(component.products()).toEqual(mockProducts);
  });
});