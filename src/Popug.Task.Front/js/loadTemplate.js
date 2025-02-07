// Function to load the external template file and insert the template content
// Usage
//    When the DOM is fully loaded, fetch and insert the header template
//
//    document.addEventListener('DOMContentLoaded', () => {
//      loadTemplate('header.html', 'header-template', '#header-placeholder');

function loadTemplate(url, templateId, targetSelector) {
  fetch(url)
  .then(response => {
      if (!response.ok) {
          throw new Error(`Could not load ${url}`);
      }
      return response.text();
  }).then(htmlString => {
      // Create a temporary container for the fetched HTML
      const tempDiv = document.createElement('div');
      tempDiv.innerHTML = htmlString;
      
      // Select the template from the fetched content
      const template = tempDiv.querySelector(`#${templateId}`);
      if (!template) {
          throw new Error(`Template with ID "${templateId}" not found in ${url}`);
      }
      
      // Clone the content of the template
      const clone = template.content.cloneNode(true);
      
      // Insert the cloned template into the target element in the main document
      document.querySelector(`#${targetSelector}`).appendChild(clone);
  })
  .catch(error => console.error('Error loading template:', error));
}