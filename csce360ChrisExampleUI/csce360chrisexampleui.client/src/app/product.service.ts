import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Product {
  companyName: string;
  productName: string;
  price: number;
  category: string;
  onSale: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class ProductService {
  constructor(private http: HttpClient) {}

  // Later: accept a filters object and append as HttpParams here.
  getProducts(): Observable<Product[]> {
    return this.http.get<Product[]>('/products');
  }
}
