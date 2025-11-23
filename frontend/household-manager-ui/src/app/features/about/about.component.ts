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
        <div class="col-lg-8 col-xl-7">
          <h1 class="text-center mb-4">About Household Manager</h1>

          <div class="card shadow-sm">
            <div class="card-body p-4 p-md-5">
              <h3 class="mb-3">What is Household Manager?</h3>
              <p class="lead">
                Household Manager is a web application designed to help families and households
                organize their daily tasks and chores efficiently. Built as an educational project
                to demonstrate modern web development practices.
              </p>

              <h4 class="mt-4 mb-3">Key Features</h4>
              <ul class="list-unstyled">
                <li class="mb-2">
                  <i class="fas fa-check text-success me-2"></i>
                  Belong to multiple households as owner or member
                </li>
                <li class="mb-2">
                  <i class="fas fa-check text-success me-2"></i>
                  Invite members, remove them, or transfer ownership
                </li>
                <li class="mb-2">
                  <i class="fas fa-check text-success me-2"></i>
                  Organize tasks by rooms with photos
                </li>
                <li class="mb-2">
                  <i class="fas fa-check text-success me-2"></i>
                  Create one-time or recurring tasks (daily, weekly, monthly, yearly) with flexible scheduling
                </li>
                <li class="mb-2">
                  <i class="fas fa-check text-success me-2"></i>
                  Auto-assign tasks fairly among household members
                </li>
                <li class="mb-2">
                  <i class="fas fa-check text-success me-2"></i>
                  Track task completion with photo proof and full execution history
                </li>
                <li class="mb-2">
                  <i class="fas fa-check text-success me-2"></i>
                  Export tasks to iCal (.ics) or subscribe to live calendar feed
                </li>
              </ul>

              <h4 class="mt-4 mb-3">How It Works</h4>
              <ol class="mb-4">
                <li class="mb-2">Sign up with your email or social account</li>
                <li class="mb-2">Create your household or join an existing one via invite code</li>
                <li class="mb-2">Add rooms to organize your living space</li>
                <li class="mb-2">Create tasks with due dates or recurring schedules</li>
                <li class="mb-2">Assign tasks manually or use auto-assign feature</li>
                <li class="mb-2">Complete tasks and optionally upload photo proof</li>
                <li class="mb-2">Sync tasks to your calendar for reminders</li>
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

    h4 {
      color: #0d6efd;
      font-weight: 600;
    }

    .list-unstyled li {
      font-size: 1.05rem;
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