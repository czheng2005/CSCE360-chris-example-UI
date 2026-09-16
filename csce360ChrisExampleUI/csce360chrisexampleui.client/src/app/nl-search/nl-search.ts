import { Component } from '@angular/core';
import { Product, ProductService } from '../product.service';

@Component({
  selector: 'app-nl-search',
  templateUrl: './nl-search.html',
  standalone: false,
  styleUrl: './nl-search.css'
})
export class NlSearch {
  query = '';
  submitting = false;
  errorMessage: string | null = null;
  summary: string | null = null;
  results: Product[] = [];

  constructor(private productService: ProductService) {}

  onSubmit(): void {
    const trimmed = this.query.trim();
    if (!trimmed) return;

    this.submitting = true;
    this.errorMessage = null;
    this.summary = null;

    this.productService.nlSearch(trimmed).subscribe({
      next: (result) => {
        this.summary = result.summary;
        this.results = result.products;
        this.submitting = false;
      },
      error: (err) => {
        console.error(err);
        this.errorMessage = 'Something went wrong running that search.';
        this.submitting = false;
      }
    });
  }
}