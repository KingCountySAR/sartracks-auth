import { createStore, applyMiddleware, combineReducers } from 'redux'
import thunkMiddleware from 'redux-thunk'
import { createLogger } from 'redux-logger'

import { reducer as oidc } from './redux/oidc'
import { reducer as accounts } from './redux/accounts'

const defaultState = {
  oidc: { signedIn: false, user: null }
}

const middleware = [
  thunkMiddleware
]

if(process.env.NODE_ENV === 'development' || (localStorage && localStorage.showLogging)) {
	const loggerMiddleware = createLogger({ collapsed: true });
	middleware.push(loggerMiddleware);
}

const rootReducer = combineReducers({
  oidc,
  accounts
})

const store = createStore(
  rootReducer,
  defaultState,
  applyMiddleware(...middleware)
)

export default store