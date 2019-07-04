import 'bootstrap/dist/css/bootstrap.css';
import '@fortawesome/fontawesome-free/css/all.min.css';
import React from 'react';
import ReactDOM from 'react-dom';
import { BrowserRouter } from 'react-router-dom';
import { Provider } from 'react-redux';
import { loadUser, OidcProvider } from 'redux-oidc';
// import { Log as OidcLog } from 'oidc-client';

import store from './store'
import userManager from './user-manager'
import { unregister as unregisterServiceWorker } from './registerServiceWorker';
import { ActionsFactory as oidcActionsFactory } from './redux/oidc';

import App from './App';

const baseUrl = document.getElementsByTagName('base')[0].getAttribute('href');
const rootElement = document.getElementById('root');

// OidcLog.logger = console;
// OidcLog.level = OidcLog.DEBUG;

function silentSignin() {
  store.dispatch({type: 'redux-oidc/LOADING_USER'});
  userManager.signinSilent().then(user => {
    // Nothing to do, handled by oidc-client-js internally
    }, err => {
        userManager.events._raiseSilentRenewError(err);
  });
}

loadUser(store, userManager)
.then(user => {
  if (!user) {
    silentSignin();
    if (window.reactConfig.oidc) {
      const actions = oidcActionsFactory(userManager);
      store.dispatch(actions.preloadUser(window.reactConfig.oidc));
    }
  }
}).catch((err) => {
  console.log('loaduser catch', err)
  silentSignin();
})

ReactDOM.render(
  <Provider store={store}>
    <i style={{display:'none'}} className='fas fa-spinner' />{/*load the icon font as soon as possible */}
    <OidcProvider store={store} userManager={userManager}>
      <BrowserRouter basename={baseUrl}>
        <App />
      </BrowserRouter>
    </OidcProvider>
  </Provider>,
  rootElement);

unregisterServiceWorker();