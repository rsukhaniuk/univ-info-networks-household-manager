import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-privacy',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="container py-5">
      <div class="row justify-content-center">
        <div class="col-lg-8 col-xl-7">
          <h1 class="text-center mb-4">Privacy Policy</h1>

          <div class="card shadow-sm">
            <div class="card-body p-4 p-md-5">
              <p class="text-muted mb-4">
                <strong>Last updated:</strong> November 23, 2025
              </p>

              <h4 class="mt-4 mb-3">Information We Collect</h4>
              <p>
                This is an educational project developed as part of a university course.
                We collect and store the following information:
              </p>
              <ul>
                <li>Email address and name (via Auth0 authentication)</li>
                <li>Profile picture (if provided through social login)</li>
                <li>Household and task data you create</li>
                <li>Photos uploaded for task completion or room images</li>
              </ul>

              <h4 class="mt-4 mb-3">How We Use Your Information</h4>
              <p>
                Your information is used solely for the functionality of the application:
              </p>
              <ul>
                <li>Authentication and authorization</li>
                <li>Managing your households, rooms, and tasks</li>
                <li>Displaying your profile to household members</li>
                <li>Generating calendar subscriptions for task schedules</li>
              </ul>

              <h4 class="mt-4 mb-3">Third-Party Services</h4>
              <p>
                We use the following third-party services:
              </p>
              <ul>
                <li><strong>Auth0</strong> - for secure authentication</li>
                <li><strong>AWS</strong> - for application hosting and data storage</li>
              </ul>
              <p>
                Your data is <strong>not sold or shared</strong> with any other third parties.
              </p>

              <h4 class="mt-4 mb-3">Data Security</h4>
              <p>
                We implement industry-standard security measures:
              </p>
              <ul>
                <li>HTTPS encryption for all communications</li>
                <li>OAuth 2.0 / OpenID Connect authentication</li>
                <li>Secure token-based API authorization</li>
                <li>Data isolation between households</li>
              </ul>

              <h4 class="mt-4 mb-3">Your Rights</h4>
              <p>You have the right to:</p>
              <ul>
                <li>Access and view your personal data</li>
                <li>Update your profile information</li>
                <li>Delete your account and all associated data</li>
                <li>Export your task data via calendar subscription</li>
              </ul>

              <h4 class="mt-4 mb-3">Data Retention</h4>
              <p>
                Your data is retained as long as your account is active.
                Upon account deletion, all your personal data and uploaded photos
                are permanently removed from our systems.
              </p>

              <h4 class="mt-4 mb-3">Contact</h4>
              <p>
                This application is developed for educational purposes.
                For questions about this privacy policy, please contact the developer.
              </p>
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

    p, li {
      line-height: 1.8;
    }

    ul {
      margin-left: 20px;
      margin-bottom: 1rem;
    }
  `]
})
export class PrivacyComponent {}