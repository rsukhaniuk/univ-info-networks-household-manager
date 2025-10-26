// src/app/shared/components/footer/footer.component.ts
import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-footer',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <footer class="footer mt-auto">
      <div class="container py-4">
        <div class="row align-items-center">
          <!-- Left: Brand -->
          <div class="col-md-4 text-center text-md-start mb-3 mb-md-0">
            <h5 class="footer-brand mb-2">
              <i class="fas fa-home me-2"></i>
              Household Manager
            </h5>
            <p class="footer-tagline mb-0">
              Organize your home, simplify your life.
            </p>
          </div>

          <!-- Center: Links -->
          <div class="col-md-4 text-center mb-3 mb-md-0">
            <div class="footer-links">
              <a routerLink="/about" class="footer-link">
                <i class="fas fa-info-circle me-1"></i>About
              </a>
              <a routerLink="/privacy" class="footer-link">
                <i class="fas fa-shield-alt me-1"></i>Privacy
              </a>
              <a href="https://github.com/yourusername/household-manager" 
                 target="_blank" 
                 class="footer-link">
                <i class="fab fa-github me-1"></i>GitHub
              </a>
            </div>
          </div>

          <!-- Right: Copyright & Social -->
          <div class="col-md-4 text-center text-md-end">
            <p class="footer-copyright mb-2">
              &copy; {{ currentYear }} Household Manager
            </p>
            <div class="footer-social">
              <a href="#" class="social-icon" title="Twitter">
                <i class="fab fa-twitter"></i>
              </a>
              <a href="#" class="social-icon" title="Facebook">
                <i class="fab fa-facebook"></i>
              </a>
              <a href="#" class="social-icon" title="LinkedIn">
                <i class="fab fa-linkedin"></i>
              </a>
            </div>
          </div>
        </div>
      </div>
    </footer>
  `,
  styles: [`
    .footer {
      background: linear-gradient(135deg, #1e3c72 0%, #2a5298 100%);
      color: #fff;
      box-shadow: 0 -2px 10px rgba(0, 0, 0, 0.1);

      .footer-brand {
        color: #fff;
        font-weight: 700;
        font-size: 1.25rem;
        margin-bottom: 0.5rem;

        i {
          color: #3498db;
        }
      }

      .footer-tagline {
        color: rgba(255, 255, 255, 0.8);
        font-size: 0.9rem;
      }

      .footer-links {
        display: flex;
        justify-content: center;
        gap: 1.5rem;
        flex-wrap: wrap;
      }

      .footer-link {
        color: rgba(255, 255, 255, 0.9);
        text-decoration: none;
        font-weight: 500;
        transition: all 0.3s ease;
        display: inline-flex;
        align-items: center;

        &:hover {
          color: #3498db;
          transform: translateY(-2px);
        }

        i {
          font-size: 0.9rem;
        }
      }

      .footer-copyright {
        color: rgba(255, 255, 255, 0.8);
        font-size: 0.875rem;
        margin-bottom: 0.5rem;
      }

      .footer-social {
        display: flex;
        justify-content: center;
        justify-content: flex-end;
        gap: 1rem;
      }

      .social-icon {
        display: inline-flex;
        align-items: center;
        justify-content: center;
        width: 36px;
        height: 36px;
        border-radius: 50%;
        background: rgba(255, 255, 255, 0.1);
        color: #fff;
        text-decoration: none;
        transition: all 0.3s ease;

        &:hover {
          background: #3498db;
          transform: translateY(-3px) scale(1.1);
          box-shadow: 0 4px 12px rgba(52, 152, 219, 0.4);
        }

        i {
          font-size: 1rem;
        }
      }
    }

    @media (max-width: 768px) {
      .footer {
        .footer-social {
          justify-content: center;
        }

        .footer-links {
          flex-direction: column;
          gap: 0.75rem;
        }
      }
    }
  `]
})
export class FooterComponent {
  currentYear = new Date().getFullYear();
}