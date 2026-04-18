// Vector maps initialization
export function initVectorMaps() {
    if (typeof jsVectorMap !== 'undefined' && document.getElementById('world_map')) {
        new jsVectorMap({
            map: 'world',
            selector: '#world_map'
        });
    }
}