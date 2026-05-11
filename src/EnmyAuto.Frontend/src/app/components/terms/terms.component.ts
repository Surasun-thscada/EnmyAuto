import { Component } from '@angular/core';

@Component({
  selector: 'app-terms',
  template: `
    <div class="max-w-3xl mx-auto px-6 py-12">
      <h1 class="text-3xl font-bold mb-6">Terms of Service</h1>
      <p class="text-sm text-gray-500 mb-8">Last updated: May 2025</p>

      <section class="mb-6">
        <h2 class="text-xl font-semibold mb-2">1. Acceptance of Terms</h2>
        <p>By accessing or using EnmyAuto, you agree to be bound by these Terms of Service.</p>
      </section>

      <section class="mb-6">
        <h2 class="text-xl font-semibold mb-2">2. Use of Service</h2>
        <p>You may use EnmyAuto only for lawful purposes and in accordance with these Terms. You agree not to misuse or attempt to disrupt the service.</p>
      </section>

      <section class="mb-6">
        <h2 class="text-xl font-semibold mb-2">3. TikTok Integration</h2>
        <p>Our service integrates with TikTok's API. By using TikTok-related features, you also agree to TikTok's Terms of Service and Platform Policy.</p>
      </section>

      <section class="mb-6">
        <h2 class="text-xl font-semibold mb-2">4. Intellectual Property</h2>
        <p>All content and materials available through EnmyAuto are the property of EnmyAuto or its licensors.</p>
      </section>

      <section class="mb-6">
        <h2 class="text-xl font-semibold mb-2">5. Limitation of Liability</h2>
        <p>EnmyAuto shall not be liable for any indirect, incidental, or consequential damages arising from your use of the service.</p>
      </section>

      <section class="mb-6">
        <h2 class="text-xl font-semibold mb-2">6. Changes to Terms</h2>
        <p>We reserve the right to modify these terms at any time. Continued use of the service constitutes acceptance of the updated terms.</p>
      </section>

      <section class="mb-6">
        <h2 class="text-xl font-semibold mb-2">7. Contact</h2>
        <p>If you have any questions about these Terms, please contact us.</p>
      </section>
    </div>
  `,
})
export class TermsComponent {}
