// File: DynamicDashboardFE/wwwroot/js/animations.js

// Celebration animation with confetti
function playCelebrationAnimation() {
    const container = document.getElementById('celebrationContainer');
    if (!container) return;

    // Clear any existing confetti
    container.innerHTML = '';

    // Colors for confetti
    const colors = [
        '#3B82F6', '#10B981', '#F59E0B', '#EF4444', '#EC4899',
        '#8B5CF6', '#6366F1', '#14B8A6', '#F97316', '#06B6D4'
    ];

    // Create confetti pieces
    const confettiCount = 100;

    for (let i = 0; i < confettiCount; i++) {
        const confetti = document.createElement('div');
        confetti.className = 'confetti';
        confetti.style.backgroundColor = colors[Math.floor(Math.random() * colors.length)];

        // Random sizes
        const size = Math.random() * 10 + 5;
        confetti.style.width = `${size}px`;
        confetti.style.height = `${size}px`;

        // Randomize shapes
        if (Math.random() > 0.5) {
            confetti.style.borderRadius = '50%';
        } else if (Math.random() > 0.5) {
            confetti.style.width = `${size / 2}px`;
            confetti.style.height = `${size}px`;
            confetti.style.transform = `rotate(${Math.random() * 90}deg)`;
        }

        // Position randomly at the top
        confetti.style.left = `${Math.random() * 100}%`;
        confetti.style.top = '-20px';

        container.appendChild(confetti);

        // Animate
        setTimeout(() => {
            confetti.style.transition = `all ${1 + Math.random() * 3}s ease-out`;
            confetti.style.opacity = '0';
            confetti.style.transform = `translateY(${window.innerHeight}px) rotate(${Math.random() * 360}deg)`;
            confetti.style.left = `${parseFloat(confetti.style.left) + (Math.random() * 40 - 20)}%`;
        }, Math.random() * 500);

        // Remove after animation
        setTimeout(() => {
            if (confetti.parentNode === container) {
                container.removeChild(confetti);
            }
        }, 4000);
    }
}

// Register the function globally
window.animations = {
    playCelebration: playCelebrationAnimation
};