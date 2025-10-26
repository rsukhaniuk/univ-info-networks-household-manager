import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-about',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="container py-5">
      <div class="row justify-content-center">
        <div class="col-lg-8">
          <h1 class="text-center mb-4">About Household Manager</h1>

          <div class="card shadow-sm">
            <div class="card-body p-5">
              <h3 class="mb-3">What is Household Manager?</h3>
              <p class="lead">
                Household Manager is a web application designed to help families and households
                organize their daily tasks and chores efficiently.
              </p>

              <h4 class="mt-4 mb-3">Key Features:</h4>
              <ul class="list-unstyled">
                <li class="mb-2">
                  <i class="fas fa-check text-success me-2"></i>
                  Create and manage households
                </li>
                <li class="mb-2">
                  <i class="fas fa-check text-success me-2"></i>
                  Invite family members and roommates
                </li>
                <li class="mb-2">
                  <i class="fas fa-check text-success me-2"></i>
                  Organize tasks by rooms
                </li>
                <li class="mb-2">
                  <i class="fas fa-check text-success me-2"></i>
                  Assign tasks to household members
                </li>
                <li class="mb-2">
                  <i class="fas fa-check text-success me-2"></i>
                  Track task completion with photos
                </li>
              </ul>

              <h4 class="mt-4 mb-3">How It Works:</h4>
              <ol class="mb-4">
                <li class="mb-2">Create your household or join an existing one</li>
                <li class="mb-2">Add rooms to organize your space</li>
                <li class="mb-2">Create tasks with due dates</li>
                <li class="mb-2">Assign tasks to household members</li>
                <li class="mb-2">Complete tasks and share progress with photos</li>
              </ol>

              <div class="text-center mt-5">
                <a routerLink="/" class="btn btn-primary btn-lg">
                  Get Started Today
                </a>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .card {
      border: none;
      border-radius: 12px;
    }

    .list-unstyled li {
      font-size: 1.1rem;
    }

    ol li {
      font-size: 1.05rem;
      line-height: 1.8;
    }

    .btn-primary {
      padding: 12px 40px;
    }
  `]
})
export class AboutComponent {}