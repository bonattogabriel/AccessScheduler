window.getCurrentPosition = () => {
    return new Promise((resolve, reject) => {
        if (navigator.geolocation) {
            navigator.geolocation.getCurrentPosition(
                position => {
                    resolve({
                        latitude: position.coords.latitude,
                        longitude: position.coords.longitude
                    });
                },
                error => {
                    console.warn('Geolocation error:', error);
                    resolve({
                        Latitude: -22.9068,
                        Longitude: -43.1729
                    });
                }
            );
        } else {
            resolve({
                latitude: -22.9068,
                longitude: -43.1729
            });
        }
    });
};