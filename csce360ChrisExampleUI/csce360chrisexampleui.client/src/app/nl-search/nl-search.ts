import { Component } from '@angular/core';

@Component({
  selector: 'app-nl-search',
  templateUrl: './nl-search.html',
  standalone: false,
  styleUrl: './nl-search.css'
})
export class NlSearch {
  query = '';
  submitting = false;
  message: string | null = null;

  onSubmit(): void {
    const trimmed = this.query.trim();
    if (!trimmed) {
      return;
    }

    this.submitting = true;
    this.message = null;

    // TODO: replace this stub with a call to the BFF's MCP-backed
    // natural-language filter endpoint once the LLM tool integration
    // is wired up (e.g. POST /Products/nl-search -> LLM -> Product/search).
    setTimeout(() => {
      this.submitting = false;
      this.message =
        'Natural language search is coming soon — this will use an LLM to translate your query into product filters.';
    }, 400);
  }
}