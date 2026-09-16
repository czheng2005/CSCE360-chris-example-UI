import { Component, OnInit, signal } from '@angular/core';
import { Product, ProductFilters, ProductService } from './product.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.html',
  standalone: false,
  styleUrl: './app.css'
})
export class App implements OnInit {
  public products = signal<Product[]>([]);
  public loading = signal(true);
  public error = signal<string | null>(null);

  public categories = signal<string[]>([]);
  public companies = signal<string[]>([]);

  // Plain (non-signal) properties: ngModel two-way binding needs a
  // settable property, not a signal function.
  public selectedCategory = '';
  public selectedCompany = '';
  public onSaleOnly = false;
  public minPrice: number | null = null;
  public maxPrice: number | null = null;

  protected readonly title = signal('csce360chrisexampleui.client');

  constructor(private productService: ProductService) {}

  ngOnInit() {
    this.loadFilterOptions();
    this.getProducts();
  }

  loadFilterOptions() {
    this.productService.getCategories().subscribe({
      next: (categories) => this.categories.set(categories),
      error: (err) => console.error('Could not load categories.', err)
    });

    this.productService.getCompanies().subscribe({
      next: (companies) => this.companies.set(companies),
      error: (err) => console.error('Could not load companies.', err)
    });
  }

  getProducts() {
    this.loading.set(true);
    this.error.set(null);

    const filters: ProductFilters = {
      category: this.selectedCategory || undefined,
      companyName: this.selectedCompany || undefined,
      onSale: this.onSaleOnly ? true : undefined,
      minPrice: this.minPrice ?? undefined,
      maxPrice: this.maxPrice ?? undefined
    };

    this.productService.getProducts(filters).subscribe({
      next: (result) => {
        this.products.set(result);
        this.loading.set(false);
      },
      error: (err) => {
        console.error(err);
        this.error.set('Could not load products.');
        this.loading.set(false);
      }
    });
  }

  onFilterChange() {
    this.getProducts();
  }

  clearFilters() {
    this.selectedCategory = '';
    this.selectedCompany = '';
    this.onSaleOnly = false;
    this.minPrice = null;
    this.maxPrice = null;
    this.getProducts();
  }
}