/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ['./src/**/*.{html,razor}'],
  theme: {
    extend: {
      screens: {
        browser: {raw: '(display-mode:browser)'},
        standalone: {raw: '(display-mode:standalone)'},
      },
      data: {
        active: 'active="true"',
      },
    },
  },
  plugins: [
    require('@tailwindcss/typography'),
    require('@tailwindcss/forms'),
  ],
}
