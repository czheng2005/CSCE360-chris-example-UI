import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Product {
  companyName: string;
  productName: string;
  price: number;
  category: string;
  onSale: boolean;
}

export interface ProductFilters {
  minPrice?: number;
  maxPrice?: number;
  category?: string;
  onSale?: boolean;
  companyName?: string;
}

@Injectable({
  providedIn: 'root'
})
export class ProductService {
  constructor(private http: HttpClient) {}

  getProducts(filters?: ProductFilters): Observable<Product[]> {
    let params = new HttpParams();

    if (filters) {
      if (filters.minPrice != null) {
        params = params.set('minPrice', filters.minPrice);
      }
      if (filters.maxPrice != null) {
        params = params.set('maxPrice', filters.maxPrice);
      }
      if (filters.category) {
        params = params.set('category', filters.category);
      }
      if (filters.onSale != null) {
        params = params.set('onSale', filters.onSale);
      }
      if (filters.companyName) {
        params = params.set('companyName', filters.companyName);
      }
    }

    return this.http.get<Product[]>('/products', { params });
  }

  getCategories(): Observable<string[]> {
    return this.http.get<string[]>('/categories');
  }

  getCompanies(): Observable<string[]> {
    return this.http.get<string[]>('/companies');
  }
}