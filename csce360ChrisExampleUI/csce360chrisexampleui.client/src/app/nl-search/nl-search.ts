import { Component, signal } from '@angular/core';
import { Product, ProductService } from '../product.service';

@Component({
  selector: 'app-nl-search',
  templateUrl: './nl-search.html',
  standalone: false,
  styleUrl: './nl-search.css'
})
export class NlSearch {
  query = '';
  submitting = signal(false);
  errorMessage = signal<string | null>(null);
  summary = signal<string | null>(null);
  results = signal<Product[]>([]);

  constructor(private productService: ProductService) {}

  onSubmit(): void {
    const trimmed = this.query.trim();
    if (!trimmed) return;

    this.submitting.set(true);
    this.errorMessage.set(null);
    this.summary.set(null);

    this.productService.nlSearch(trimmed).subscribe({
      next: (result) => {
        this.summary.set(result.summary);
        this.results.set(result.products);
        this.submitting.set(false);
      },
      error: (err) => {
        console.error(err);
        this.errorMessage.set('Something went wrong running that search.');
        this.submitting.set(false);
      }
    });
  }
}