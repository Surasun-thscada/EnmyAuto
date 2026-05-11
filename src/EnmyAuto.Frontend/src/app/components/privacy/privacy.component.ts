import { Component } from '@angular/core';

@Component({
  selector: 'app-privacy',
  template: `
    <div class="max-w-3xl mx-auto px-6 py-12">
      <h1 class="text-3xl font-bold mb-6">Privacy Policy</h1>
      <p class="text-sm text-gray-500 mb-8">Last updated: May 2025</p>

      <section class="mb-6">
        <h2 class="text-xl font-semibold mb-2">1. Information We Collect</h2>
        <p>We collect information you provide directly to us, such as your name, email address, and account credentials. We may also collect information from TikTok when you connect your account.</p>
      </section>

      <section class="mb-6">
        <h2 class="text-xl font-semibold mb-2">2. How We Use Your Information</h2>
        <p>We use the information we collect to provide, maintain, and improve our services, process transactions, and communicate with you.</p>
      </section>

      <section class="mb-6">
        <h2 class="text-xl font-semibold mb-2">3. TikTok Data</h2>
        <p>When you connect your TikTok account, we access only the data necessary to provide our services. We do not sell or share your TikTok data with third parties.</p>
      </section>

      <section class="mb-6">
        <h2 class="text-xl font-semibold mb-2">4. Data Retention</h2>
        <p>We retain your information for as long as your account is active or as needed to provide services. You may request deletion of your data at any time.</p>
      </section>

      <section class="mb-6">
        <h2 class="text-xl font-semibold mb-2">5. Security</h2>
        <p>We implement appropriate security measures to protect your personal information against unauthorized access or disclosure.</p>
      </section>

      <section class="mb-6">
        <h2 class="text-xl font-semibold mb-2">6. Cookies</h2>
        <p>We use cookies and similar technologies to operate and improve our service. You can control cookies through your browser settings.</p>
      </section>

      <section class="mb-6">
        <h2 class="text-xl font-semibold mb-2">7. Contact Us</h2>
        <p>If you have questions about this Privacy Policy or wish to exercise your data rights, please contact us.</p>
      </section>
    </div>
  `,
})
export class PrivacyComponent {}
