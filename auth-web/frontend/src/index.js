import 'bootstrap/dist/css/bootstrap.css';
import '@fortawesome/fontawesome-free/css/all.min.css';
import React from 'react';
import ReactDOM from 'react-dom';
import { BrowserRouter } from 'react-router-dom';
import { Provider } from 'react-redux';
import { loadUser, OidcProvider } from 'redux-oidc';
//import { Log as OidcLog } from 'oidc-client';

import store from './store'
import userManager from './user-manager'
import registerServiceWorker from './registerServiceWorker';

import App from './App';

const baseUrl = document.getElementsByTagName('base')[0].getAttribute('href');
const rootElement = document.getElementById('root');

//OidcLog.logger = console;
//OidcLog.level = OidcLog.DEBUG;

function silentSignin() {
  store.dispatch({type: 'redux-oidc/LOADING_USER'});
  userManager.signinSilent();
}

loadUser(store, userManager)
.then(user => {
  !user && silentSignin();
}).catch(() => {
  silentSignin();
})

ReactDOM.render(
  <Provider store={store}>
    <OidcProvider store={store} userManager={userManager}>
      <BrowserRouter basename={baseUrl}>
        <App />
      </BrowserRouter>
    </OidcProvider>
  </Provider>,
  rootElement);

registerServiceWorker();
