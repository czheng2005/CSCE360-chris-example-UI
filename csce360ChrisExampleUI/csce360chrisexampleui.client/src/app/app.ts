import { Component, OnInit, signal } from '@angular/core';
import { Product, ProductService } from './product.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.html',
  standalone: false,
  styleUrl: './app.css'
})
export class App implements OnInit {
  // Define state as reactive signals
  public products = signal<Product[]>([]);
  public loading = signal(true);
  public error = signal<string | null>(null);
  
  protected readonly title = signal('csce360chrisexampleui.client');

  constructor(private productService: ProductService) {}

  ngOnInit() {
    this.getProducts();
  }

  getProducts() {
    this.loading.set(true);
    this.error.set(null);
    
    this.productService.getProducts().subscribe({
      next: (result) => {
        // Ensure 'result' is actually an array here. If it's wrapped, use result.propertyName
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
}