import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-privacy',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="container py-5">
      <div class="row justify-content-center">
        <div class="col-lg-8">
          <h1 class="mb-4">Privacy Policy</h1>
          
          <div class="card shadow-sm">
            <div class="card-body p-4">
              <p class="lead mb-4">
                <strong>Last updated:</strong> {{ currentDate | date:'longDate' }}
              </p>

              <h4 class="mt-4 mb-3">Information We Collect</h4>
              <p>
                This is an educational project. We collect and store your email, name,
                and household data to operate the application.
              </p>

              <h4 class="mt-4 mb-3">How We Use Your Information</h4>
              <p>
                Your information is used solely for the functionality of the application:
              </p>
              <ul>
                <li>Authentication and authorization</li>
                <li>Managing your household and task data</li>
                <li>Displaying your profile information</li>
              </ul>

              <h4 class="mt-4 mb-3">Data Sharing</h4>
              <p>
                Your information is <strong>not shared with third parties</strong>. 
                Data is only visible to members of your household.
              </p>

              <h4 class="mt-4 mb-3">Data Security</h4>
              <p>
                We use industry-standard security measures including:
              </p>
              <ul>
                <li>HTTPS encryption for all communications</li>
                <li>OAuth 2.0 / OpenID Connect authentication (Auth0)</li>
                <li>Secure token-based authorization</li>
              </ul>

              <h4 class="mt-4 mb-3">Your Rights</h4>
              <p>You have the right to:</p>
              <ul>
                <li>Access your personal data</li>
                <li>Update or delete your account</li>
                <li>Export your household data</li>
              </ul>

              <h4 class="mt-4 mb-3">Contact</h4>
              <p>
                If you have questions about this privacy policy, please contact us at:
                <br>
                <a href="mailto:privacy@householdmanager.com">
                  privacy@householdmanager.com
                </a>
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
    }

    a {
      color: #0d6efd;
      text-decoration: none;
      
      &:hover {
        text-decoration: underline;
      }
    }
  `]
})
export class PrivacyComponent {
  currentDate = new Date();
}