function getSettings(param){
    switch (param){
        case 'AzSearchIndexName':
            return 'YOUR_AZURE_SEARCH_INDEX_NAME';
            break;

        case 'AzSearchKey':
            return 'YOUR_AZURE_SEARCH_KEY';
            break;

        case 'AzSearchServiceName':
            return 'YOUR_AZURE_SEARCH_NAME';
            break;
    }
}