/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ['./src/**/*.{html,razor}'],
  theme: {
    extend: {},
  },
  plugins: [
    require('@tailwindcss/typography'),
    require('@tailwindcss/forms'),
  ],
}
