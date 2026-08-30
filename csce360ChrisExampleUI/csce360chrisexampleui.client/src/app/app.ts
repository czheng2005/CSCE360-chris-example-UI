import { Component, OnInit, signal } from '@angular/core';
import { Product, ProductService } from './product.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.html',
  standalone: false,
  styleUrl: './app.css'
})
export class App implements OnInit {
  public products: Product[] = [];
  public loading = true;
  public error: string | null = null;

  constructor(private productService: ProductService) {}

  ngOnInit() {
    this.getProducts();
  }

  getProducts() {
    this.loading = true;
    this.error = null;
    this.productService.getProducts().subscribe({
      next: (result) => {
        this.products = result;
        this.loading = false;
      },
      error: (err) => {
        console.error(err);
        this.error = 'Could not load products.';
        this.loading = false;
      }
    });
  }

  protected readonly title = signal('csce360chrisexampleui.client');
}
